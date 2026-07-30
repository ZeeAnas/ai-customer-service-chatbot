using Chatbot.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Chatbot.Api.Data;

public class ChatbotDbContext : DbContext
{
    public ChatbotDbContext(
        DbContextOptions<ChatbotDbContext> options
    ) : base(options)
    {
    }

    public DbSet<Conversation> Conversations =>
        Set<Conversation>();

    public DbSet<Message> Messages =>
        Set<Message>();

    public DbSet<Lead> Leads =>
        Set<Lead>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder
    )
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Conversation>(entity =>
        {
            entity.HasKey(
                conversation => conversation.Id
            );

            entity.Property(
                    conversation => conversation.SessionId
                )
                .IsRequired()
                .HasMaxLength(200);

            entity.HasIndex(
                    conversation => conversation.SessionId
                )
                .IsUnique();

            entity.HasMany(
                    conversation => conversation.Messages
                )
                .WithOne(
                    message => message.Conversation
                )
                .HasForeignKey(
                    message => message.ConversationId
                )
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(message => message.Id);

            entity.Property(message => message.Role)
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(message => message.Content)
                .IsRequired();
        });

        modelBuilder.Entity<Lead>(entity =>
        {
            entity.HasKey(lead => lead.Id);

            entity.Property(lead => lead.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(lead => lead.Email)
                .HasMaxLength(254);

            entity.Property(lead => lead.Phone)
                .HasMaxLength(30);

            entity.Property(lead => lead.Message)
                .IsRequired()
                .HasMaxLength(1000);

            entity.Property(lead => lead.Status)
                .IsRequired();

            entity.Property(lead => lead.StaffNotes)
                .HasMaxLength(2000);

            entity.Property(lead => lead.CreatedAtUtc)
                .IsRequired();

            entity.Property(lead => lead.UpdatedAtUtc)
                .IsRequired();
                

            entity.HasIndex(lead => lead.Status);

            entity.HasIndex(lead => lead.CreatedAtUtc);

            entity.HasOne(lead => lead.Conversation)
                .WithMany(
                    conversation => conversation.Leads
                )
                .HasForeignKey(
                    lead => lead.ConversationId
                )
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}