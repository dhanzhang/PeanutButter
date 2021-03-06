﻿using System;
using EmailSpooler.Win32Service.Entity;
using PeanutButter.RandomGenerators;
// ReSharper disable InconsistentNaming
// ReSharper disable ClassNeverInstantiated.Global

namespace EmailSpooler.Win32Service.Tests.Builders
{
    public class EmailBuilder: GenericBuilder<EmailBuilder,Email>
    {
        public override EmailBuilder WithRandomProps()
        {
            return base.WithRandomProps()
                        .WithProp(e => e.Sent = false)
                        .WithProp(e => e.SendAt = DateTime.Now.AddMinutes(-1))
                        .WithProp(e => e.SendAttempts = 0);
        }
        public EmailBuilder WithRandomCC(int howMany = 1)
        {
                WithProp(e =>
                    {
                        for (var i = 0; i < howMany; i++)
                            e.EmailRecipients.Add(EmailRecipientBuilder.Create().WithRandomProps()
                                                                            .WithProp(r =>
                                                                            {
                                                                                r.IsPrimaryRecipient = false;
                                                                                r.IsCC = true;
                                                                                r.IsBCC = false;
                                                                            }).Build());
                    });
            return this;
        }
        public static Email BuildRandomWithRecipient()
        {
            return Create().WithRandomProps().WithRandomRecipient().Build();
        }
        public EmailBuilder WithRandomRecipient(int howMany = 1)
        {
                WithProp(e =>
                    {
                        for (var i = 0; i < howMany; i++)
                            e.EmailRecipients.Add(EmailRecipientBuilder.Create().WithRandomProps()
                                .WithProp(r =>
                                {
                                    r.IsPrimaryRecipient = true;
                                    r.IsCC = false;
                                    r.IsBCC = false;
                                }).Build());
                });
            return this;
        }
        public EmailBuilder WithRandomBCC(int howMany = 1)
        {
                WithProp(e =>
                    {
                        for (var i = 0; i < howMany; i++)
                            e.EmailRecipients.Add(EmailRecipientBuilder.Create().WithRandomProps()
                                                                            .WithProp(r =>
                                                                            {
                                                                                r.IsPrimaryRecipient = false;
                                                                                r.IsCC = false;
                                                                                r.IsBCC = true;
                                                                            }).Build());
                    });
            return this;
        }
        public EmailBuilder WithRandomAttachment(int howMany = 1)
        {
            WithProp(e =>
                {
                    for (var i = 0; i < howMany; i++)
                        e.EmailAttachments.Add(EmailAttachmentBuilder.BuildRandom());
                });
            return this;
        }
    }
}
